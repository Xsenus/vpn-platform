import test from 'node:test'
import assert from 'node:assert/strict'
import React from 'react'
import { renderToStaticMarkup } from 'react-dom/server'
import {
  CodeBlock,
  ConfirmButton,
  CopyButton,
  DataTableLite,
  EmptyState,
  ErrorBlock,
  ExternalLinkActions,
  FormValidationSummary,
  getSafeHttpUrl,
  getSafeSvgImageDataUrl,
  LoadingBlock,
  PageShell,
  PasswordField,
  PrimaryButton,
  QrCodePreview,
  SecretField,
  SectionCard,
  SegmentedTabs,
  SkipLink,
  StateBlock,
  StatusBadge,
  formatStatusLabel,
  tryWriteClipboardText,
  ValidationModeBadge,
  designTokens
} from '../packages/ui/src/index.tsx'

test('PageShell renders title and children', () => {
  const html = renderToStaticMarkup(
    <PageShell title="Hello">
      <div>World</div>
    </PageShell>
  )

  assert.match(html, /Hello/)
  assert.match(html, /World/)
  assert.match(html, /id="main-content"/)
  assert.match(html, /tabIndex="-1"|tabindex="-1"/)
})

test('StatusBadge and CodeBlock render content', () => {
  const html = renderToStaticMarkup(
    <div>
      <StatusBadge value="Active" />
      <CodeBlock>vpn://example</CodeBlock>
      <PrimaryButton type="button">Buy</PrimaryButton>
    </div>
  )

  assert.match(html, /Активно/)
  assert.match(html, /vpn:\/\/example/)
  assert.match(html, /Buy/)
})

test('formatStatusLabel localizes status text outside badges', () => {
  assert.equal(formatStatusLabel('PendingPayment'), 'Ожидает оплаты')
  assert.equal(formatStatusLabel('sandbox'), 'Проверка')
  assert.equal(formatStatusLabel('Production'), 'Рабочий режим')
  assert.equal(formatStatusLabel('NewSubscription'), 'Новая подписка')
  assert.equal(formatStatusLabel('Web'), 'Сайт')
  assert.equal(formatStatusLabel('inbound'), 'От пользователя')
  assert.equal(formatStatusLabel('fixed'), 'Исправлено')
  assert.equal(formatStatusLabel('Archived'), 'Архив')
  assert.equal(formatStatusLabel(null), 'Неизвестно')
})

test('StatusBadge tones distinguish composite negative and successful states', () => {
  const renderStatus = (value: string) => renderToStaticMarkup(<StatusBadge value={value} />)

  assert.match(renderStatus('Unhealthy'), /status-badge-danger/)
  assert.match(renderStatus('Inactive'), /status-badge-danger/)
  assert.match(renderStatus('NotLinked'), /status-badge-neutral/)
  assert.match(renderStatus('Degraded'), /status-badge-warning/)
  assert.match(renderStatus('Succeeded'), /status-badge-success/)
  assert.match(renderStatus('PartiallyRefunded'), /status-badge-success/)
  assert.match(renderStatus('open'), /Открыто/)
  assert.match(renderStatus('pending'), /Ожидает/)
  assert.match(renderStatus('closed'), /Закрыто/)
})

test('UI polish helpers render loading, empty, error, copy and validation states', () => {
  const html = renderToStaticMarkup(
    <SectionCard title="Security" description="Secrets stay write-only">
      <ValidationModeBadge />
      <LoadingBlock label="Loading admin dashboard" />
      <SkipLink />
      <ErrorBlock message="API error" />
      <EmptyState title="No payments" description="Create a checkout first" />
      <CopyButton value="vless://sandbox" />
      <PasswordField label="Password" value="secret" onChange={() => {}} help="Use 8 characters" />
      <ConfirmButton message="Удалить запись?" onConfirm={() => {}}>Удалить</ConfirmButton>
      <SecretField label="Webhook secret" configured={true} value="" maxLength={4096} />
    </SectionCard>
  )

  assert.match(html, /Security/)
  assert.match(html, /Проверочный режим: внешние действия отключены/)
  assert.match(html, /Loading admin dashboard/)
  assert.match(html, /skip-link/)
  assert.match(html, /Перейти к содержимому/)
  assert.match(html, /href="#main-content"/)
  assert.match(html, /API error/)
  assert.match(html, /No payments/)
  assert.match(html, /Скопировать/)
  assert.match(html, /copy-action/)
  assert.match(html, /copy-feedback/)
  assert.match(html, /role="status"/)
  assert.match(html, /aria-label="Скопировать: скопировать значение в буфер обмена"/)
  assert.match(html, /password-field-row/)
  assert.match(html, /class="form-field"/)
  assert.match(html, /aria-describedby=/)
  assert.match(html, /aria-pressed="false"/)
  assert.match(html, /maxLength="4096"/)
  assert.match(html, /Показать/)
  assert.doesNotMatch(html, /<label[^>]*>(?:(?!<\/label>)[\s\S])*password-toggle/)
  assert.match(html, /aria-expanded="false"/)
  assert.match(html, /aria-controls=/)
  assert.match(html, /aria-haspopup="dialog"/)
  assert.match(html, /aria-busy="false"/)
  assert.match(html, /Удалить/)
  assert.match(html, /Задано|Webhook secret/)
  assert.doesNotMatch(html, /raw-password|PRIVATE KEY|bot-token/i)
})

test('clipboard boundary reports success, unavailable API and denied permission', async () => {
  let written = ''
  assert.equal(await tryWriteClipboardText('vpn://success', { writeText: async (value) => { written = value } }), true)
  assert.equal(written, 'vpn://success')
  assert.equal(await tryWriteClipboardText('vpn://unavailable', null), false)
  assert.equal(await tryWriteClipboardText('vpn://denied', { writeText: async () => { throw new Error('permission denied') } }), false)
})

test('external link boundary exposes only absolute credential-free http URLs', () => {
  assert.equal(getSafeHttpUrl(' https://pay.example.test/checkout?id=1 '), 'https://pay.example.test/checkout?id=1')
  assert.equal(getSafeHttpUrl('http://127.0.0.1:5173/payment-sandbox'), 'http://127.0.0.1:5173/payment-sandbox')
  assert.equal(getSafeHttpUrl('javascript:alert(1)'), null)
  assert.equal(getSafeHttpUrl('data:text/html,payment'), null)
  assert.equal(getSafeHttpUrl('/relative-payment'), null)
  assert.equal(getSafeHttpUrl('https://user:secret@pay.example.test/checkout'), null)

  const validHtml = renderToStaticMarkup(
    <ExternalLinkActions value="https://pay.example.test/checkout" openLabel="Открыть оплату" />
  )
  assert.match(validHtml, /href="https:\/\/pay\.example\.test\/checkout"/)
  assert.match(validHtml, /rel="noopener noreferrer"/)
  assert.match(validHtml, /Скопировать ссылку/)

  const rejectedHtml = renderToStaticMarkup(
    <ExternalLinkActions value="javascript:alert(1)" openLabel="Открыть оплату" invalidMessage="Ссылка отклонена" />
  )
  assert.match(rejectedHtml, /role="alert"/)
  assert.match(rejectedHtml, /Ссылка отклонена/)
  assert.doesNotMatch(rejectedHtml, /href=|javascript:|Скопировать ссылку/)
})

test('QR preview isolates validated SVG as an image and rejects active markup', () => {
  const safeSvg = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><rect width="10" height="10" /></svg>'
  const dataUrl = getSafeSvgImageDataUrl(safeSvg)
  assert.match(dataUrl ?? '', /^data:image\/svg\+xml;charset=utf-8,/)

  const html = renderToStaticMarkup(<QrCodePreview svg={safeSvg} label="QR тест" />)
  assert.match(html, /<img/)
  assert.match(html, /alt="QR тест"/)
  assert.match(html, /width="220"/)
  assert.match(html, /height="220"/)
  assert.doesNotMatch(html, /dangerouslySetInnerHTML|<svg/)

  for (const unsafeSvg of [
    '<svg><script>alert(1)</script></svg>',
    '<svg onload="alert(1)"></svg>',
    '<svg><foreignObject>html</foreignObject></svg>',
    '<svg><use href="https://tracker.example.test/image" /></svg>',
    '<html>not svg</html>',
    `<svg>${'x'.repeat(1_000_001)}</svg>`
  ]) {
    assert.equal(getSafeSvgImageDataUrl(unsafeSvg), null)
  }

  const rejectedHtml = renderToStaticMarkup(<QrCodePreview svg="<svg onload='alert(1)'></svg>" />)
  assert.match(rejectedHtml, /role="alert"/)
  assert.doesNotMatch(rejectedHtml, /<img|onload|alert\(1\)/)
})

test('Design system primitives render shared tabs, states and tables', () => {
  const html = renderToStaticMarkup(
    <div>
      <SegmentedTabs
        idPrefix="test-auth"
        panelId="test-panel"
        label="Режим авторизации"
        value="login"
        onChange={() => {}}
        options={[
          { value: 'login', label: 'Вход' },
          { value: 'register', label: 'Регистрация' }
        ]}
      />
      <StateBlock tone="warning" title="Нужно внимание" description="Проверьте настройки" />
      <FormValidationSummary errors={['Заполните название', 'Укажите корректную цену']} />
      <DataTableLite
        columns={['Колонка', 'Статус']}
        rows={[[<span key="value">Значение</span>, <StatusBadge key="status" value="Ready" />]]}
      />
      <DataTableLite columns={['Пусто']} rows={[]} emptyTitle="Нет записей" emptyDescription="Создайте первую запись" />
    </div>
  )

  assert.equal(designTokens.colors.primary, 'var(--primary)')
  assert.equal(designTokens.radius.md, 'var(--radius-md)')
  assert.match(html, /role="tablist"/)
  assert.match(html, /id="test-auth-login-tab"/)
  assert.match(html, /aria-selected="true"/)
  assert.match(html, /aria-label="Статус: Готово"/)
  assert.match(html, /state-block-warning/)
  assert.match(html, /form-validation-summary/)
  assert.match(html, /Проверьте поля формы/)
  assert.match(html, /data-table-lite/)
  assert.match(html, /Нет записей/)
})

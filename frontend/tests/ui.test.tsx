import test from 'node:test'
import assert from 'node:assert/strict'
import React from 'react'
import { renderToStaticMarkup } from 'react-dom/server'
import {
  CodeBlock,
  ConfirmButton,
  CopyButton,
  EmptyState,
  ErrorBlock,
  LoadingBlock,
  PageShell,
  PasswordField,
  PrimaryButton,
  SecretField,
  SectionCard,
  SkipLink,
  StatusBadge,
  ValidationModeBadge
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
      <SecretField label="Webhook secret" configured={true} value="" />
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
  assert.match(html, /password-field-row/)
  assert.match(html, /class="form-field"/)
  assert.match(html, /aria-pressed="false"/)
  assert.match(html, /Показать/)
  assert.doesNotMatch(html, /<label[^>]*>(?:(?!<\/label>)[\s\S])*password-toggle/)
  assert.match(html, /aria-expanded="false"/)
  assert.match(html, /aria-controls=/)
  assert.match(html, /aria-busy="false"/)
  assert.match(html, /Удалить/)
  assert.match(html, /Задано|Webhook secret/)
  assert.doesNotMatch(html, /raw-password|PRIVATE KEY|bot-token/i)
})

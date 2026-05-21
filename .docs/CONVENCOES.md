# 📋 Convenções de Documentação

> **Premissa Fundamental:** Toda documentação do projeto **SEMPRE** fica na pasta `.docs/`, nunca espalhada pela raiz do repositório.

---

## 🎯 Princípios

### 1. Centralização Única
```
✅ CORRETO:     .docs/NOME_DOCUMENTO.md
❌ INCORRETO:   ./NOME_DOCUMENTO.md
❌ INCORRETO:   ./docs/NOME_DOCUMENTO.md
❌ INCORRETO:   ./src/NOME_DOCUMENTO.md
```

### 2. Documentação é Código
- Versionada junto com o código
- Revisa junto com PRs
- Atualizada quando a arquitetura muda

### 3. Single Source of Truth (SSOT)
- Não duplicar informações entre arquivos
- Usar referências cruzadas quando necessário
- INDEX.md mantém lista centralizada

---

## 📝 Categorias de Documentos

### 🏗️ Arquitetura & Design
Decisões arquiteturais, padrões, justificativas tecnológicas.

**Exemplos:**
- ARQUITETURA.md - Decisões principais
- PADROES.md - Design patterns aplicados
- ADR-001-CQRS.md - Architecture Decision Records

**Prefixo:** `ARQUITETURA_`, `PADROES_`, `ADR-`

### 🚀 Implementação & Guias
Passo-a-passo, tutoriais, como usar um framework/biblioteca.

**Exemplos:**
- GUIA_IMPLEMENTACAO_WOLVERINE.md - Tutorial passo-a-passo
- SETUP_DESENVOLVIMENTO.md - Configurar ambiente
- INTEGRACAO_RABBITMQ.md - Guias específicos

**Prefixo:** `GUIA_`, `SETUP_`, `INTEGRACAO_`

### 📖 Referências Técnicas
Referência rápida, cheat sheets, quick guides.

**Exemplos:**
- WOLVERINE_QUICK_REFERENCE.md - Quick reference
- POSTGRESQL_CHEATSHEET.md - Cheat sheet
- API_ENDPOINTS.md - Endpoints disponíveis

**Prefixo:** `QUICK_`, `CHEATSHEET_`, `REFERENCE_`

### 🔐 Segurança & Compliance
Políticas de segurança, compliance, secrets management.

**Exemplos:**
- SEGURANCA_REQUISITOS.md
- COMPLIANCE_CHECKLIST.md
- SECRETS_POLICY.md

**Prefixo:** `SEGURANCA_`, `COMPLIANCE_`, `SECRETS_`

### 📊 Análise & Relatórios
Análises técnicas, estudos comparativos, benchmarks.

**Exemplos:**
- ANALISE_PERFORMANCE.md
- COMPARATIVO_FRAMEWORKS.md
- BENCHMARK_CACHE.md

**Prefixo:** `ANALISE_`, `COMPARATIVO_`, `BENCHMARK_`

---

## 📐 Template Padrão

```markdown
# 📄 Título Descritivo com Emoji

> **Objetivo:** Uma frase clara do que este documento faz.
> **Audiência:** Quem deve ler (iniciantes/implementadores/arquitetos)
> **Última atualização:** Mês Ano

---

## 📋 Índice

- [Seção 1](#seção-1)
- [Seção 2](#seção-2)
- [Referências](#referências)

---

## Seção 1

Conteúdo com exemplos práticos.

```csharp
// Exemplo de código
var exemplo = new Classe();
```

## Seção 2

Mais conteúdo.

---

## Referências

- 🔗 [Link 1](url)
- 🔗 [Link 2](url)

---

**Versão:** 1.0  
**Status:** ✅ Ativo | ⚠️ Em Revisão | 🔄 Planejado
```

---

## ✅ Checklist: Antes de Commitar

- [ ] **Localização:** Arquivo em `.docs/`?
- [ ] **Nomenclatura:** Segue convenção (PascalCase)?
- [ ] **Conteúdo:**
  - [ ] Título com emoji
  - [ ] Objetivo claro
  - [ ] Índice de seções
  - [ ] Exemplos práticos
  - [ ] Links internos (.docs/)
  - [ ] Data de atualização
- [ ] **Qualidade:**
  - [ ] Sem typos/erros
  - [ ] Formatação Markdown consistente
  - [ ] Código compilaria/rodaria se necessário
- [ ] **Manutenção:**
  - [ ] INDEX.md atualizado?
  - [ ] Referências cruzadas corretas?
  - [ ] Sem conteúdo duplicado?

---

## 🚫 Não Faça

```markdown
❌ Não crie documentação na raiz do repositório
❌ Não duplique conteúdo em múltiplos arquivos
❌ Não use documentação desatualizada como referência
❌ Não ignore links quebrados
❌ Não misture idiomas (português/inglês)
✅ Mantenha tudo em .docs/
✅ Use referências cruzadas
✅ Atualize INDEX.md
✅ Revise links antes de commitar
```

---

## 🔗 Exemplos de Referências Cruzadas

**Dentro de `.docs/`:**
```markdown
Veja [ARQUITETURA.md](ARQUITETURA.md#seção) para mais detalhes.
```

**Para fora de `.docs/`:**
```markdown
Veja [README.md](../README.md) para instruções de execução.
Veja [DOCKER-COMPOSE.md](../DOCKER-COMPOSE.md) para setup.
```

---

## 📚 Estrutura Final Esperada

```
.docs/
├── INDEX.md                              (← Você está aqui)
├── CONVENÇÕES.md                         (Guia de convenções)
├── ARQUITETURA.md                        (Decisões principais)
├── GUIA_IMPLEMENTACAO_WOLVERINE.md       (Tutorial)
├── WOLVERINE_QUICK_REFERENCE.md          (Cheat sheet)
└── desafio-arquiteto-software.pdf        (Referência)
```

---

**Premissa Fundamental:** Documentação centralizada = documentação mantida = documentação útil.

Toda nova documentação SEMPRE em `.docs/`

---

**Versão:** 1.0  
**Status:** ✅ Ativo  
**Última atualização:** Maio 2026

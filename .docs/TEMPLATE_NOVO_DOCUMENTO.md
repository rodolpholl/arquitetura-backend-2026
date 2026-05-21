# 📄 TEMPLATE_NOVO_DOCUMENTO.md

> **Objetivo:** Este é um template padrão para criar nova documentação na pasta `.docs/`.
> 
> **Como usar:** Copie este arquivo, renomeie para `CATEGORIA_NOME.md` e preencha seguindo a estrutura.
> 
> **Audiência:** Toda a equipe (iniciantes, implementadores, arquitetos)
> 
> **⚠️ Lembre-se:** Toda documentação SEMPRE em `.docs/` (premissa fundamental)

---

## 📋 Índice

- [Introdução](#introdução)
- [Pré-requisitos](#pré-requisitos)
- [Seção 1 - Conceitos](#seção-1---conceitos)
- [Seção 2 - Exemplos Práticos](#seção-2---exemplos-práticos)
- [Melhores Práticas](#melhores-práticas)
- [Troubleshooting](#troubleshooting)
- [Referências](#referências)

---

## Introdução

**Escreva aqui uma introdução clara do que este documento trata.**

Exemplo:
- O que é (definição)
- Por que importa (relevância)
- Quando usar (contexto)

---

## Pré-requisitos

**Liste dependências, conhecimentos ou ferramentas necessárias:**

- Conhecimento de: X, Y, Z
- Ferramentas instaladas: X, Y
- Arquivos necessários: X, Y

---

## Seção 1 - Conceitos

### 1.1 Conceito Chave 1

**Explicação clara com exemplo:**

```csharp
// Exemplo de código
public class Exemplo
{
    public void Metodo()
    {
        // Implementação
    }
}
```

### 1.2 Conceito Chave 2

**Mais explicações:**

```bash
# Comando de exemplo
dotnet build
```

---

## Seção 2 - Exemplos Práticos

### Exemplo 1: Cenário Simples

**Descrição:** O que este exemplo faz.

**Código:**
```csharp
// Código completo do exemplo
```

**Saída esperada:**
```
Resultado esperado aqui
```

### Exemplo 2: Cenário Avançado

**Descrição:** Caso de uso mais complexo.

**Código:**
```csharp
// Código mais complexo
```

---

## Melhores Práticas

### ✅ Faça

```csharp
// Bom exemplo
public class Bom
{
    // Implementação correta
}
```

- Ponto 1
- Ponto 2
- Ponto 3

### ❌ Não Faça

```csharp
// Evite isto
public class Ruim
{
    // Implementação errada
}
```

- Anti-padrão 1
- Anti-padrão 2
- Anti-padrão 3

---

## Troubleshooting

### Problema 1: Descrição

**Sintoma:** O que acontece

**Causa:** Por que acontece

**Solução:** Como resolver

```bash
# Comando para resolver
```

### Problema 2: Descrição

**Sintoma:** Outro problema

**Causa:** Causa raiz

**Solução:** Passo a passo

---

## Referências

### Links Internos (.docs/)
- 🔗 [INDEX.md](INDEX.md) - Índice de documentação
- 🔗 [CONVENCOES.md](CONVENCOES.md) - Convenções de documentação
- 🔗 [ARQUITETURA.md](ARQUITETURA.md) - Arquitetura do projeto

### Links Internos (Raiz)
- 🔗 [README.md](../README.md) - Overview do projeto
- 🔗 [DOCKER-COMPOSE.md](../DOCKER-COMPOSE.md) - Infraestrutura

### Documentação Externa
- 📖 [Framework Oficial](https://example.com) - Documentação
- 📖 [Tutorial Externo](https://example.com) - Referência
- 🎥 [Vídeo Tutorial](https://example.com) - Visual learning

---

## 🔄 Workflow Padrão

```
1. Copiar TEMPLATE_NOVO_DOCUMENTO.md
   ↓
2. Renomear para CATEGORIA_NOME.md
   ↓
3. Preencher cada seção
   ↓
4. Testar links (especialmente cruzados)
   ↓
5. Atualizar INDEX.md
   ↓
6. Commitar em .docs/
```

---

## 📋 Checklist Antes de Finalizar

- [ ] Título com emoji apropriado
- [ ] Objetivo claro no topo
- [ ] Índice completo e funcionando
- [ ] Exemplos práticos inclusos
- [ ] Sem erros de digitação
- [ ] Links internos funcionam
- [ ] Links externos referenciam URLs corretas
- [ ] Data de atualização informada
- [ ] INDEX.md atualizado com este novo documento
- [ ] CONVENCOES.md consultadas

---

## 💡 Dicas Úteis

### Estrutura de Títulos
```markdown
# Nível 1: Título principal com emoji
## Nível 2: Seções principais
### Nível 3: Subseções
#### Nível 4: Itens específicos
```

### Formatar Código
```markdown
## Inline
Use `código inline` para palavras/identificadores

## Bloco com Linguagem
\`\`\`csharp
public class Exemplo { }
\`\`\`

## Bloco sem Linguagem
\`\`\`
Saída genérica
\`\`\`
```

### Tabelas
```markdown
| Coluna 1 | Coluna 2 | Coluna 3 |
|----------|----------|----------|
| Valor 1  | Valor 2  | Valor 3  |
| Valor 4  | Valor 5  | Valor 6  |
```

### Boxes de Atenção
```markdown
> **⚠️ Aviso:** Algo importante

> **ℹ️ Informação:** Algo útil

> **✅ Sucesso:** Resultado esperado

> **❌ Erro:** Problema comum
```

---

## 📞 Suporte

**Dúvidas sobre este template?**

1. Consulte [CONVENCOES.md](CONVENCOES.md) - Convenções de documentação
2. Veja [INDEX.md](INDEX.md) - Outros documentos como referência
3. Compare com documentos existentes em `.docs/`

---

**Versão:** 1.0  
**Status:** 📄 Template  
**Última atualização:** Maio 2026  
**Localização:** `.docs/` ✅

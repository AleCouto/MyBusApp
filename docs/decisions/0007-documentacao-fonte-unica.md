# 0007 — Documentação com fonte única e skills só para procedimentos

**Estado:** aceite · 2026-11-10

## Contexto

O projeto acumulou três planos concorrentes com numeração de milestones
incompatível, oito ficheiros de "skills" que nenhum agente descobria
automaticamente (sem `SKILL.md` nem frontmatter) e referências a código que já
não existia. O custo não era ter documentação a mais: era ter documentação
contraditória, que um agente segue com confiança.

## Decisão

Uma fonte por tipo de conhecimento, organizada pelo ciclo de vida:

- `AGENTS.md` — índice e regras, sempre carregado, uma página.
- `docs/architecture.md` — como o código está (muda com o código).
- `docs/domain.md` — semântica dos conceitos (muda raramente).
- `docs/conventions.md` — como se escreve código (muda raramente).
- `docs/roadmap.md` — o único plano ativo.
- `docs/decisions/` — ADRs, append-only: o porquê.
- `docs/archive/` — planos substituídos, congelados.
- `.github/skills/<nome>/SKILL.md` — apenas procedimentos que valem a pena
  carregar quando a tarefa aparece (formato Agent Skills, com frontmatter).

Cada documento declara `Verificado: <data> @ <commit>`. Adaptadores por
ferramenta (`.github/copilot-instructions.md`, symlinks para `.claude/skills`)
não contêm conhecimento: só apontam.

## Consequências

- Menos ficheiros e uma única resposta a cada pergunta.
- Atualizar documentação passa a fazer parte do commit que muda o código.
- O histórico de decisões não se reescreve; acrescenta-se.

## Alternativas rejeitadas

- Uma cópia por harness: multiplica a manutenção e garante divergência.
- Wiki externa: sai do repositório, não é versionada com o código e nenhum
  agente a lê.

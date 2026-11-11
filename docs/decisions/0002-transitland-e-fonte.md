# 0002 — Transitland é fonte, não operador

**Estado:** aceite · registado retroativamente em 2026-11-10

## Contexto

O Transitland agrega rotas de várias empresas. Apresentá-lo na interface como
se fosse uma transportadora induz o utilizador em erro e esconde o operador
real (por exemplo, Carris).

## Decisão

O Transitland é uma fonte/agregador técnico no domínio dos autocarros. Continua
a ser um `BusProvider` internamente, porque é preciso para roteamento e para a
identidade dos favoritos. Na interface, o nome do operador real só aparece
quando essa informação está disponível e validada; caso contrário, não se
atribui nem se inventa um nome.

## Consequências

- A UI pode mostrar uma linha sem indicar empresa — é preferível a mentir.
- Os favoritos guardam o provider técnico, mas não o usam como rótulo.
- A resolução de nomes de operador exige trabalho adicional por fonte.

## Alternativas rejeitadas

- Mostrar "Transitland" como operador: confunde agregador com transportadora.
- Esconder o provider por completo: impede resolver consultas e identificar
  favoritos de forma estável.

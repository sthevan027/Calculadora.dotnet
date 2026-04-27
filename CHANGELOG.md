# Changelog

Todas as mudanças relevantes do projeto serão documentadas aqui.

O formato segue uma variação simples de *Keep a Changelog*.

## [Unreleased]

### Adicionado

- README com instruções de execução/compilação e notas de persistência.

## [0.1.0] - 2026-04-27

### Adicionado

- Calculadora WPF com UI custom (sem “glass”) e temas **claro/escuro** (inclui opção **Sistema**).
- Tela de **Configurações** com abas **Geral** e **Científica**.
- **Histórico** opcional com persistência em arquivo.
- Modo **Básico** e **Científico** com avaliação de expressões.
- Suporte a **graus/radianos** no modo científico.
- **Percentual contextual** no modo científico (ex.: `200 + 10%`).
- Ícone vetorial (`AppIcon`) aplicado nas janelas.

### Corrigido

- `ComboBox` custom: dropdown não abria por template incompleto; popup ajustado para overlay mais consistente.

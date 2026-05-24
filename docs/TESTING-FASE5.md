# Guia de teste — Fase 5 (widgets reais)

Substituímos o HelloWidget placeholder pelos três widgets de produção:
radar circular, spotter lateral e painel Relative. Esta fase é
exclusivamente validação visual — nenhum risco novo.

Tempo: **10 minutos** se o overlay da Fase 4 já está funcionando.

---

## Pré-requisitos

Os mesmos da Fase 4 (iRacing em Windowed Borderless, .NET 8 SDK, etc.).
Se a Fase 4 estava OK pra você, está tudo pronto.

---

## Rodar

```powershell
cd $HOME\Documents\iCorre-overlay
git pull
dotnet build iRadar.sln -c Release
dotnet run --project src/iRadar.App -c Release
```

Com iRacing aberto em **Replay**, você deve ver agora 4 widgets sobre o
iRacing (em vez de apenas o HelloWidget de antes):

1. **Status** — canto superior esquerdo, painel pequeno com Track / Tick /
   Cars / State / Dots / Spotter (substitui o "iRadar — Fase 4")
2. **Radar** — abaixo do Status, círculo com triângulo verde no centro
   (você) e bolinhas coloridas representando carros próximos
3. **Relative** — direita do Status, lista textual `+0.34  #42  Driver`
   com seções AHEAD e BEHIND
4. **Spotter** — barras verticais nas laterais do overlay; em **replay
   ficam invisíveis** (iRacing não computa CarLeftRight em replay, como
   visto na Fase 1). Para validar é preciso entrar em sessão ao vivo (Test
   Drive offline serve).

---

## Checklist de validação

### Status widget
- [ ] Aparece no canto superior esquerdo
- [ ] Track mostra o nome correto do circuito
- [ ] Tick aumenta a cada frame
- [ ] Cars mostra o número total de carros
- [ ] State mostra `replay` em modo replay (ou `on-track` / `pit/garage`
      em sessão ao vivo)

### Radar widget
- [ ] **Retângulo branco vertical** maior no centro (player), com borda
      escura sutil
- [ ] Dois círculos concêntricos (linha cinza) representam os anéis de
      distância — o externo é **15m** (range total reduzido para focar
      em perigos iminentes), o interno é **7m**, label "7m" perto do
      interno
- [ ] Cruz cinza pelo centro (horizontal e vertical) para referência
- [ ] **Retângulos verticais coloridos** maiores aparecem onde há carros
      próximos:
  - cinza fraco quando longe (Safe)
  - **laranja** quando perto (Close)
  - **vermelho** quando muito perto (Danger)
- [ ] **Halo direcional ao redor do PLAYER** (não mais ao redor dos
      outros carros): quando há um carro Close/Danger no radar, aparece
      um halo translúcido **deslocado em direção ao carro mais
      ameaçador**. Cor laranja para Close, vermelha para Danger.
- [ ] Player rectangle continua sempre **centralizado** — só o halo
      "vaza" para o lado da ameaça
- [ ] Em replay com vários carros próximos: dots espalhados em 5 "lanes"
      laterais (heurístico, não posição real) para diferenciação visual.
      Com range reduzido para 15m, o espalhamento fica ainda mais visível
- [ ] Em sessão ao vivo: dots laterais usam o spotter real do iRacing
      (precisão real quando há `CarLeft`/`CarRight`)
- [ ] Quando o replay avança, retângulos se movem suavemente

### Relative widget
- [ ] Aparece à direita do Status
- [ ] Seção `AHEAD` com até 3 carros com gap positivo (em vermelho)
- [ ] Separador
- [ ] Seção `BEHIND` com até 3 carros com gap negativo (em verde)
- [ ] Formato: `+0.34  #42  Driver Name` (gap com 2 decimais, número do
      carro, primeiros 16 chars do nome)
- [ ] Carros no pitlane aparecem em cinza com sufixo `(pit)`
- [ ] Quando não há ninguém em range: `(none in range)` em cinza

### Spotter (agora integrado ao RadarWidget)
A versão anterior tinha barras laterais separadas; foram removidas. Agora a
proximidade aparece como halo translúcido nos retângulos dos carros dentro
do próprio RadarWidget:

- [ ] **Em replay**: halos coloridos aparecem quando carros entram nas
      zonas Close/Danger (orange/red); o spotter LEFT/RIGHT do iRacing
      em si fica indisponível mas o visual de proximidade no radar
      compensa
- [ ] **Em sessão ao vivo** (Test Drive + IA): o spotter do iRacing
      também acrescenta posicionamento lateral mais preciso (carro mais
      próximo ganha hint de lado, conforme `CarLeftRight`); halos
      continuam funcionando independente

### Click-through (regressão da Fase 4)
- [ ] Cursor passando pelos widgets não muda comportamento
- [ ] Clicar sobre qualquer widget chega no iRacing (não rouba foco)

### Performance
- [ ] FPS do iRacing não cai notavelmente com o overlay ativo
- [ ] Sem stutter visível ao mover a câmera do replay

---

## Limitações conhecidas desta fase

- **Posições fixas**: você não consegue ainda mover ou redimensionar os
  widgets — é tudo posicionado em coordenadas fixas dentro do overlay
  window. Edit Mode (com hotkey `Ctrl+Alt+E`) é a próxima fase (Fase 6).

- **Lateral aproximado**: como o iRacing não expõe (x, y) absolutos para
  outros carros, a posição lateral dos dots no radar é uma aproximação:
  - **Em sessão ao vivo**: o carro mais próximo (dentro de 20m) recebe
    posição lateral real baseada na hint `CarLeftRight` do iRacing
    (±3.5m do centro); demais carros ficam alinhados ao eixo central
    ou recebem heurístico se o spotter geral estiver inativo.
  - **Em replay**: o spotter do iRacing não está disponível, então
    aplicamos um espalhamento heurístico em 5 lanes (~±3.5m no total)
    baseado em CarIdx. É puramente cosmético — distingue visualmente
    carros agrupados — mas não corresponde à posição lateral real.
  - Em curvas isto pode parecer impreciso — Fase 7+ pode adicionar
    geometria de pista para refinar.

- **Em replay o spotter do iRacing não funciona** — mas os halos
  laranja/vermelho no RadarWidget compensam visualmente.

---

## Como reportar de volta

- **Tudo OK**: `OK Fase 5` e sigo para Fase 6 (Edit Mode + configuração
  persistente)
- **Algo errado**: cola o que está vendo + as últimas linhas do
  `%LocalAppData%\iRadar\debug.log` que tiver mensagens `[overlay]`

Screenshots ajudam bastante se for problema visual (posicionamento,
cores, recorte da janela).

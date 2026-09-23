# Caffeine estimate model

The quiz facade is `CaffeineCalculator`. Pure arithmetic lives in `CaffeineEstimate`, immutable coefficients in `CaffeineCalculationConfig`, and localized snapshot formatting in `ConsumptionResultBuilder`. The existing interface and quiz flow are preserved. Coffee and tea round to the nearest 5 mg (midpoints away from zero); energy drinks retain integer rounding. Invalid, non-finite or unrepresentable amounts are rejected rather than producing corrupt results.

## Home coffee

Previously: grams × 12 × bean multiplier (Arabica 1, Robusta 1.7) × extraction. Old extraction factors were EspressoMachine .75, ColdBrew .80, MokaPot .70, FrenchPress .75, Turkish .85, PourOver .70, Kettle .75.

Now: grams × bean caffeine mg/g × extraction. Arabica is 13.5 mg/g; Robusta is 24.5 mg/g. The full entered dose is one consumption; cup volume/count is not involved.

| Method | Extraction |
| --- | ---: |
| EspressoMachine | 0.58 |
| FrenchPress | 0.95 |
| PourOver | 0.90 |
| MokaPot | 0.85 |
| Turkish | 0.90 |
| ColdBrew | 0.90 |
| Kettle | 0.90 |

Extraction is constrained to [0,1]. The rounded result cannot exceed the caffeine in the dry dose: for exceptionally small doses the integer physical ceiling takes precedence over 5 mg rounding. Large doses are not capped at a typical cup's caffeine.

## Coffee outside

Previously: drink-specific mg/100 ml × final volume / 100 × bean multiplier. Old mg/100 ml: Espresso 212, Macchiato 100, Americano 45, Cappuccino 40, Latte 32, FlatWhite 55, Mocha 38, Affogato 100.

Now: estimated shots × 60 mg (Arabica) or 100 mg (Robusta). No additional dilution or brewing multiplier is used. Water and milk do not create or remove the espresso base's caffeine. Chocolate in Mocha is not separately estimated because the quiz does not know its amount.

| Drink | Small shots | Medium shots | Large shots | Existing volumes, ml |
| --- | ---: | ---: | ---: | --- |
| Espresso | 0.5 | 1 | 2 | 15 / 30 / 60 |
| Macchiato | 1 | 1 | 2 | 30 / 60 / 90 |
| Americano | 1 | 2 | 3 | 150 / 250 / 350 |
| Cappuccino | 1 | 2 | 2 | 150 / 250 / 350 |
| Latte | 1 | 2 | 2 | 200 / 300 / 400 |
| FlatWhite | 2 | 2 | 3 | 150 / 200 / 250 |
| Mocha | 1 | 2 | 2 | 200 / 300 / 400 |
| Affogato | 1 | 1 | 2 | 60 / 90 / 120 |

Preset sizes use the recipe directly. Manual volume uses piecewise linear interpolation between that drink's actual preset points. Below Small, scale proportionally; above Large, extend the last segment (including a flat segment for Latte/Cappuccino/Mocha). Clamp manual estimates to 0.5–4 shots. Espresso instead uses volume / 30 with the same clamp. Thus actual Americano 200 ml maps to 1.5 shots, or 90 mg Arabica. At 240 ml it maps to 1.9 shots, or 115 mg after rounding. Volume remains available in the result display.

## Tea

Previously: grams × 6 mg/g Black or 5 mg/g Green; a spoon was 2.5 g.

Now: grams × 20 mg/g Black or 15 mg/g Green, representing caffeine extracted during typical preparation. One spoon defaults to 2 g, so 1/2/3 spoons yield Black 40/80/120 mg or Green 30/60/90 mg. Previously saved recipe grams remain the source of truth: an old 2.5 g recipe is still 2.5 g and is recalculated with the new coefficient.

## Verification

No test project existed. Added a standalone .NET 10 regression executable that links production source and substitutes localization strings only:

```powershell
dotnet run --project tests/CaffeineCalculation.Checks
```

58,225 assertions pass: all enum profiles, preset/manual anchors for both beans, interpolation continuity, shot bounds, physical bounds, dose/bean monotonicity, invalid/overflow inputs, result facade and recipe round trips.

| Case | Result, mg |
| --- | ---: |
| Home 8 g Arabica espresso | 65 (raw 62.64) |
| Home 8 g Robusta espresso | 115 (raw 113.68) |
| Home 15 g Arabica FrenchPress | 190 (raw 192.375) |
| Home 15 g Arabica PourOver / Kettle / ColdBrew | 180 (raw 182.25) |
| Outside Espresso Arabica 30 / 60 ml | 60 / 120 |
| Outside Latte Arabica Medium / Large | 120 / 120 |
| Outside Americano Arabica Small / Medium / Large | 60 / 120 / 180 |
| Black tea 2 / 5 g | 40 / 100 |
| Green tea 2 g | 30 |
| Energy drink 330 ml | 106 (unchanged) |

These are practical defaults specified for this model, not universal laboratory constants. As an external sanity reference, [EFSA](https://www.efsa.europa.eu/en/topics/topic/caffeine) lists approximate examples of 80 mg per 60 ml espresso, 90 mg per 200 ml filter coffee and 50 mg per 220 ml black tea, and explicitly notes serving/concentration variability. Our double Arabica espresso estimate of 120 mg is higher than that example; we retain the requested 60 mg/shot baseline rather than treating generic serving examples as limits.

Last Recipe is tested through the actual mapper and facade for home, outside and tea. Code inspection confirms `UseLastRecipeAsync` recalculates answers; `SaveConsumptionAsync` saves the displayed snapshot's `CaffeineMg`. MainPage and Calendar consume persisted caffeine values. No persistence schema, migrations, historical consumptions, XAML or statistics formulas were changed.

`dotnet clean` and `dotnet build` succeeded with zero warnings/errors. Android is included in the build. No devices were listed by `adb devices`; real quiz interaction and SQLite/MainPage/Calendar end-to-end behavior on a phone were not exercised. Persistence/statistics wiring was verified by source inspection, not a live UI run.

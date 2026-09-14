# VagabondK.OpenType

.NET 기반의 **OpenType 폰트 파일 생성 라이브러리**입니다. 사용자가 선과 곡선으로 글리프 윤곽을 직접 구성하면, TTF와 OTF 폰트 파일을 생성합니다.

## 프로젝트 목적

이 프로젝트는 **[VagabondK.Indicators](https://github.com/Vagabond-K/VagabondK.Indicators)** 프로젝트의 **디지털 문자를 폰트로 생성**하기 위해 개발되었습니다.

`VagabondK.Indicators`는 데이터를 화면에 표시하기 위한 .NET 기반 인디케이터 라이브러리입니다. 현재 **Digital Indicator**만 제공하며(차트/Analog Indicator 기능은 아직 구현되지 않았습니다), Digital Indicator는 수치/문자열 데이터를 디지털 문자로 표시합니다. 

본 프로젝트(VagabondK.OpenType)는 그 디지털 문자의 윤곽을 실제 OpenType 폰트 파일(OTF/TTF)로 변환해, 폰트 시스템에 설치하거나 임베디드 환경에서 텍스트 렌더링으로 재사용할 수 있게 합니다.

## 주요 기능

- **TTF + OTF(CFF) 동시 생성** — 확장자(`.ttf`/`.otf`)에 따라 자동 분기
- **플루언트 윤곽 API** — `MoveTo`/`LineTo`/`QuadTo`/`CurveTo` 체이닝으로 글리프를 그립니다
- **IList 기반 윤곽/컨투어** — `GlyphOutline : IList<GlyphContour>`, `GlyphContour : IList<ContourCommand>`로 중간 삽입·수정 지원
- **공유 컨투어 → CFF 서브루틴** — 여러 글리프가 같은 컨투어를 참조하면 CFF Private Subrs로 자동 등록되어 파일 크기 절감
- **일관된 메타데이터 cascade** — `FontBuilder`가 metrics/italic/fixedPitch/caretSlope를 hhea/OS/2/post/head(CFF Top DICT)에 일관되게 반영
- **참조 라이브러리 0개**

## 사용 예제

```csharp
using VagabondK.OpenType;
using VagabondK.OpenType.Geometry;

var outline = new GlyphOutline();
outline.BeginContour()
    .MoveTo(100, 0)
    .LineTo(100, 700)
    .LineTo(300, 700)
    .LineTo(300, 0)
    .LineTo(100, 0);

var builder = new FontBuilder()
    .UnitsPerEm(1000)
    .Family("VagabondK")
    .Subfamily("Regular")
    .UniqueId("VagabondK Regular: 1.0")
    .Metrics(new FontMetrics { Ascender = 1125, Descender = -250, LineGap = 0 })
    .FixedPitch(true);

builder.AddGlyph('1', new Glyph(outline, 400));

builder.Save("VagabondK-Regular.otf");  // .otf → OTF(CFF 1.0)
builder.Save("VagabondK-Regular.ttf");  // .ttf → TTF
```

## 샘플

| 샘플 | 설명 |
|------|------|
| `SevenSegmentSample` | `VagabondK.Indicators`의 7-segment 디지털 숫자(0~9 + 특수문자)를 OTF/TTF로 생성. Regular/Italic face 지원 |
| `SimpleFontSample` | '1'~'9'를 단순 도형(사각형, 정오각형, 타원 등)으로 그려 윤곽/곡선/공유 컨투어 동작을 검증 |

## 빌드 및 실행

```powershell
# 전체 빌드
dotnet build VagabondK.OpenType.slnx

# 샘플 실행 (bin/Debug/net10.0/ 에 폰트 파일 생성)
dotnet run --project SevenSegmentSample
dotnet run --project SimpleFontSample
```

생성된 폰트 파일은 Python의 `opentype-sanitizer`(`python -m ots <file>`)로 검증할 수 있습니다.

## 구현되지 않은 기능 (주의사항)

이 라이브러리는 디지털 숫자 폰트 생성에 필요한 최소 기능만 구현합니다. 아래 항목은 **지원하지 않으므로**, 생성된 폰트를 사용할 때 주의하세요.

- **힌팅(hinting) 없음** — post 테이블은 format 3.0(글리프 이름·힌팅 데이터 없음)이며, `gasp`/`fpgm`/`prep`/`cvt` 테이블을 생성하지 않습니다. 작은 크기(12~16px)에서 렌더링이 매끄럽지 않을 수 있습니다.
- **GPOS / GSUB 없음** — kerning, ligature, 문자 대체/위치 조정 기능 테이블을 생성하지 않습니다. 글리프 간 미세 간격 보정이 필요하면 `advanceWidth`/`leftSideBearing`으로 직접 처리하세요.
- **수직 쓰기(vertical writing) 미지원** — `vhea`/`vmtx` 테이블을 생성하지 않습니다. 수직 배치 폰트로 사용할 수 없습니다.
- **composite glyph 미지원** — TTF(glyf)는 simple glyph만 생성하며, `FontReader`도 composite glyph(`numberOfContours < 0`)를 읽지 못해 `NotSupportedException`을 던집니다.
- **셀프 리더(self-reader) 한정** — `FontReader.Load`는 **이 라이브러리가 생성한 폰트**만 역파싱합니다. 외부에서 만든 폰트(특히 cmap format 4의 non-zero `idRangeOffset`를 쓰는 폰트)는 로드할 수 없습니다.
- **cmap 범위** — format 4(BMP, U+0000–U+FFFF)와 format 12(전체 Unicode)를 함께 생성합니다. non-BMP 문자는 format 12로만 매핑됩니다.

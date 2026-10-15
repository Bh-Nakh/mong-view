# 제3자 구성요소 고지 (THIRD-PARTY NOTICES)

몽뷰는 아래 제3자 구성요소를 사용하거나 함께 배포한다. 각 구성요소는 해당 저작권자의 라이선스를 따르며, 그 조건은 몽뷰의 GPL-3.0 라이선스(`LICENSE`)와 별개로 유지된다.

## 구성요소 목록 (배포본에 포함)

| 구성요소 | 버전 | 라이선스 | 출처 | 고지 파일 위치 |
|---|---|---|---|---|
| SharpCompress | 0.50.4 | MIT | https://github.com/adamhathcock/sharpcompress | `licenses/SharpCompress/LICENSE` |

- SharpCompress 저작권 표기: Copyright (c) 2025 Adam Hathcock. 수정 없이 NuGet 패키지로 사용한다.
- 몽뷰는 net10.0-windows 대상으로 빌드하므로 SharpCompress의 하위 의존성(Microsoft.Bcl.AsyncInterfaces, System.Text.Encoding.CodePages)은 배포본에 들어오지 않는다. 다른 대상 프레임워크로 바꾸면 이 둘(MIT, Microsoft, https://github.com/dotnet/runtime)을 이 표에 추가하고 원문을 `licenses/dotnet-runtime/LICENSE` 에 보관한다.
- .NET 런타임은 설치 프로그램에 포함하지 않고 사용자 PC의 런타임을 쓴다(MIT, Microsoft).

## 테스트 전용 패키지 (배포 고지 대상 아님)

아래 패키지는 `tests/NoAdView.Tests` 에서만 쓰이며 설치본에 들어가지 않는다. 배포 고지 대상이 아니므로 참고용으로만 적는다.

| 구성요소 | 버전 | 라이선스 |
|---|---|---|
| xunit, xunit.core, xunit.assert, xunit.extensibility.core·execution | 2.9.3 | Apache-2.0 |
| xunit.runner.visualstudio | 3.1.5 | Apache-2.0 |
| xunit.analyzers | 1.18.0 | Apache-2.0 |
| xunit.abstractions | 2.0.3 | Apache-2.0 (nuspec에 SPDX 표기 없음, licenseUrl 기준) |
| Microsoft.NET.Test.Sdk, Microsoft.CodeCoverage, Microsoft.TestPlatform.ObjectModel·TestHost | 18.3.0 | MIT |
| Newtonsoft.Json | 13.0.3 | MIT |

## 라이선스 원문 보관 위치

- 저장소 기준 `licenses/{구성요소}/` 폴더에 구성요소별 `LICENSE` 파일을 받은 그대로 보관한다.
- 설치본에도 같은 폴더를 포함해 사용자가 열람할 수 있게 한다.

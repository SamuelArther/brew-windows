class Ghalint < Formula
  desc "GitHub Actions linter"
  homepage "https://github.com/suzuki-shunsuke/ghalint"
  url "https://github.com/suzuki-shunsuke/ghalint/releases/download/v1.5.6/ghalint_1.5.6_windows_amd64.zip"
  sha256 "109ea9b39c8e263cef924bd3b4fe5505964204f934ca60d986f4090d01a99ba5"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/suzuki-shunsuke/ghalint/archive/refs/tags/v1.5.6.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "ghalint.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ghalint version")

    (testpath/".github/workflows/test.yml").write <<~YAML
      name: test

      on: [push]

      jobs:
        test:
          runs-on: ubuntu-latest
          steps:
            - uses: actions/checkout@v4
    YAML

    output = shell_output("#{bin}/ghalint run .github/workflows/test.yml 2>&1", 1)
    assert_match "job should have permissions", output
  end
end

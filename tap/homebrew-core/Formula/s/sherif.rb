class Sherif < Formula
  desc "Opinionated, zero-config linter for JavaScript monorepos"
  homepage "https://github.com/QuiiBz/sherif"
  url "https://github.com/QuiiBz/sherif/releases/download/v1.13.0/sherif-x86_64-pc-windows-msvc.zip"
  sha256 "6dfa21b47f3dd316393e1e2a3c000a7f4c8c0ea30717a040003adc5ddcb239d8"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/QuiiBz/sherif/archive/refs/tags/v1.13.0.tar.gz"
  end

  def install
    bin.install "sherif.exe"
  end

  test do
    (testpath/"package.json").write <<~JSON
      {
        "name": "test",
        "version": "1.0.0",
        "private": true,
        "packageManager": "npm",
        "workspaces": [ "." ]
      }
    JSON
    (testpath/"test.js").write <<~JS
      console.log('Hello, world!');
    JS
    assert_match "No issues found", shell_output("#{bin}/sherif --no-install .")
  end
end

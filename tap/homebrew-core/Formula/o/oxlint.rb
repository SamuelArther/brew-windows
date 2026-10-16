class Oxlint < Formula
  desc "High-performance linter for JavaScript and TypeScript written in Rust"
  homepage "https://oxc.rs/"
  url "https://github.com/oxc-project/oxc/releases/download/oxlint_v1.88.0/oxlint-x86_64-pc-windows-msvc.zip"
  version "1.88.0"
  sha256 "b12ff0848a2b474b57735ae08613c603e9f8d692bafc3a67e6561abb60770bb2"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/oxc-project/oxc/archive/refs/tags/oxlint_v1.88.0.tar.gz"
    regex(/^oxlint_v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "oxlint-x86_64-pc-windows-msvc.exe" => "oxlint.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/oxlint --version 2>&1")
  end
end

class RustAnalyzer < Formula
  desc "Experimental Rust compiler front-end for IDEs"
  homepage "https://rust-analyzer.github.io/"
  url "https://github.com/rust-lang/rust-analyzer/releases/download/2026-10-12/rust-analyzer-x86_64-pc-windows-msvc.zip"
  version "2026-10-12"
  sha256 "77f7a907f0e6f179281bac38e2d08435c974db9a5b99de52c5c963aff52883ec"
  license any_of: ["Apache-2.0", "MIT"]

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "rust-analyzer.exe"
  end

  test do
    assert_match "rust-analyzer", shell_output("#{bin}/rust-analyzer --version")
  end
end

class LanguagetoolRust < Formula
  desc "LanguageTool API in Rust"
  homepage "https://docs.rs/languagetool-rust"
  url "https://github.com/jeertmans/languagetool-rust/releases/download/v3.0.1/ltrs-v3.0.1-x86_64-pc-windows-gnu.tar.gz"
  sha256 "315fd60ebad244b83dc333402a2036593da4b17bf684a791d84348e6522d9248"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jeertmans/languagetool-rust/archive/refs/tags/v3.0.1.tar.gz"
  end

  def install
    bin.install "ltrs.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ltrs --version")

    system bin/"ltrs", "ping"
    assert_match '"name": "Arabic"', shell_output("#{bin}/ltrs languages")

    output = shell_output("#{bin}/ltrs check --text \"Some phrase with a smal mistake\"")
    assert_match "error[MORFOLOGIK_RULE_EN_US]: Possible spelling mistake found", output
  end
end

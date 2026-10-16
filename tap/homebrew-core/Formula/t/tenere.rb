class Tenere < Formula
  desc "TUI interface for LLMs written in Rust"
  homepage "https://github.com/pythops/tenere"
  url "https://github.com/pythops/tenere/releases/download/v0.11.3/tenere-x86_64-pc-windows-msvc.exe"
  sha256 "ec94c87ba8707b5372730805530a933272f1462096d8912079a33fc901499605"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/pythops/tenere/archive/refs/tags/v0.11.3.tar.gz"
  end

  def install
    bin.install "tenere-x86_64-pc-windows-msvc.exe" => "tenere.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tenere --version")
    assert_match "Can not find the openai api key", shell_output("#{bin}/tenere 2>&1", 1)
  end
end

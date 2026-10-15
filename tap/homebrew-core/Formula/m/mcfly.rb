class Mcfly < Formula
  desc "Fly through your shell history"
  homepage "https://github.com/cantino/mcfly"
  url "https://github.com/cantino/mcfly/releases/download/v0.9.4/mcfly-v0.9.4-x86_64-pc-windows-msvc.zip"
  sha256 "a390a094a8dfc324f4e474f8d8eb4eed956386170242adba3eadbe3a298f8193"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/cantino/mcfly/archive/refs/tags/v0.9.4.tar.gz"
  end

  def install
    bin.install "mcfly.exe"
  end

  test do
    assert_match "mcfly_prompt_command", shell_output("#{bin}/mcfly init bash")
    assert_match version.to_s, shell_output("#{bin}/mcfly --version")
  end
end

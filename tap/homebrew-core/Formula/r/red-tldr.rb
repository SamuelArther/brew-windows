class RedTldr < Formula
  desc "Used to help red team staff quickly find the commands and key points"
  homepage "https://payloads.online/red-tldr/"
  url "https://github.com/Rvn0xsy/red-tldr/releases/download/v0.4.3/red-tldr_0.4.3_windows_amd64.tar.gz"
  sha256 "3ae3f1d0cea645be7df8c9ffb226775e88f2257ca1497a6328713ec80c7713e0"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Rvn0xsy/red-tldr/archive/refs/tags/v0.5.0.tar.gz"
  end

  def install
    bin.install "red-tldr.exe"
  end

  test do
    assert_match "privilege", shell_output("#{bin}/red-tldr mimikatz")
  end
end

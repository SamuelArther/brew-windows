class Tunnelite < Formula
  desc "Secure tunneling to local applications"
  homepage "https://tunnelite.com"
  url "https://github.com/cristipufu/tunnelite/releases/download/v1.2.1/tunnelite-win-x64.zip"
  sha256 "cdf67c4a3011cf17142b549cc9d850fab6801ec95b2024808752bda7aeaf909a"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/cristipufu/tunnelite/archive/refs/tags/v1.2.1.tar.gz"
  end

  def install
    bin.install "tunnelite.exe"
  end

  test do
    system bin/"tunnelite", "--version"
  end
end

class Ktea < Formula
  desc "Kafka TUI client"
  homepage "https://github.com/jonas-grgt/ktea"
  url "https://github.com/jonas-grgt/ktea/releases/download/v0.8.1/ktea_0.8.1_windows_amd64.tar.gz"
  sha256 "55756a5ec15624305d6b214256c998655652b37902f07b7bb50449cfc539ab80"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jonas-grgt/ktea/archive/refs/tags/v0.8.1.tar.gz"
  end

  def install
    bin.install "ktea.exe"
  end

  test do
    system bin/"ktea", "--help"
  end
end

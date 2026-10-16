class FakeGcsServer < Formula
  desc "Emulator for Google Cloud Storage API"
  homepage "https://github.com/fsouza/fake-gcs-server"
  url "https://github.com/fsouza/fake-gcs-server/releases/download/v1.56.1/fake-gcs-server_1.56.1_Windows_amd64.tar.gz"
  sha256 "79c1a92b6d666c6fc2a60439f8ebee9e01b918dad90cae6f003ade4a5877a566"
  license "BSD-2-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/fsouza/fake-gcs-server/archive/refs/tags/v1.56.1.tar.gz"
  end

  def install
    bin.install "fake-gcs-server.exe"
  end

  test do
    system bin/"fake-gcs-server", "--help"
  end
end

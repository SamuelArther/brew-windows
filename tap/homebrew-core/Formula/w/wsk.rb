class Wsk < Formula
  desc "OpenWhisk Command-Line Interface (CLI)"
  homepage "https://openwhisk.apache.org/"
  url "https://github.com/apache/openwhisk-cli/releases/download/1.2.0/OpenWhisk_CLI-1.2.0-windows-amd64.zip"
  sha256 "18504f4df74e74095b78b9829f3500be9979b655dad11ae841dd0a236413b2b8"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/apache/openwhisk-cli/archive/refs/tags/1.2.0.tar.gz"
  end

  def install
    bin.install "wsk.exe"
  end

  test do
    system bin/"wsk", "property", "set", "--apihost", "https://127.0.0.1"
  end
end

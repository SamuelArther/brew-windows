class Jid < Formula
  desc "Json incremental digger"
  homepage "https://github.com/simeji/jid"
  url "https://github.com/simeji/jid/releases/download/v1.1.3/jid_windows_amd64.zip"
  sha256 "02153d3edae1ec7bfdcfb21de5904469fac75cbbac8f6199639d31b69c8aef3b"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/simeji/jid/archive/refs/tags/v1.1.3.tar.gz"
  end

  def install
    bin.install "jid.exe"
  end

  test do
    assert_match "jid version v#{version}", shell_output("#{bin}/jid --version")
  end
end

class Sendme < Formula
  desc "Tool to send files and directories, based on iroh"
  homepage "https://iroh.computer/sendme"
  url "https://github.com/n0-computer/sendme/releases/download/v0.36.1/sendme-v0.36.1-windows-x86_64.zip"
  sha256 "c5b4c30813c65df51ebc3276d9f4f0cd9a7b1a371b081d80b12587eb558acbcb"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/n0-computer/sendme/archive/refs/tags/v0.36.1.tar.gz"
  end

  def install
    bin.install "sendme.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sendme --version 2>&1")
  end
end

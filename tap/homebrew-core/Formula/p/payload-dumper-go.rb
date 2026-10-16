class PayloadDumperGo < Formula
  desc "Android OTA payload dumper written in Go"
  homepage "https://github.com/ssut/payload-dumper-go"
  url "https://github.com/ssut/payload-dumper-go/releases/download/2.1.0/payload-dumper-go_2.1.0_windows_amd64.tar.gz"
  sha256 "45483c54bdd9219b8dcdb757256e7ce7a2bf9552d0e43f32e03389a1f0c55b15"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ssut/payload-dumper-go/archive/refs/tags/2.1.0.tar.gz"
  end

  def install
    bin.install "payload-dumper-go.exe"
  end

  test do
    system bin/"payload-dumper-go", "--help"
  end
end

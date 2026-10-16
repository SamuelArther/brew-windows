class Nats < Formula
  desc "Utility for NATS Server and JetStream administration"
  homepage "https://github.com/nats-io/natscli"
  url "https://github.com/nats-io/natscli/releases/download/v0.5.0/nats-0.5.0-windows-amd64.zip"
  sha256 "1f1ea2b5ff1acf97e0e1c486b1695edc64256233c70c4e888cce2370fd549025"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/nats-io/natscli/archive/refs/tags/v0.5.0.tar.gz"
  end

  def install
    bin.install "nats.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/nats --version")
    assert_match "No known contexts", shell_output("#{bin}/nats context ls")
    assert_match(/^[A-Z0-9]+$/, shell_output("#{bin}/nats auth nkey gen user").strip)
  end
end

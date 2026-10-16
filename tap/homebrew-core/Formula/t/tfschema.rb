class Tfschema < Formula
  desc "Schema inspector for Terraform/OpenTofu providers"
  homepage "https://github.com/minamijoyo/tfschema"
  url "https://github.com/minamijoyo/tfschema/releases/download/v0.7.10/tfschema_0.7.10_windows_amd64.tar.gz"
  sha256 "d66f178d3d7a7adc92f32f139fb26bc00d2e774f01f7295e80e18dc0a11e17fc"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/minamijoyo/tfschema/archive/refs/tags/v0.7.10.tar.gz"
  end

  def install
    bin.install "tfschema.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tfschema --version 2>&1")
  end
end

class Podlet < Formula
  desc "Generate podman quadlet files from a podman command or compose file"
  homepage "https://github.com/containers/podlet"
  url "https://github.com/containers/podlet/releases/download/v0.3.2/podlet-x86_64-pc-windows-msvc.zip"
  sha256 "54505b2c9cf356e5a3cc00381225786fa864730ca87666453aa0e3b1ca983ae8"
  license "MPL-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/containers/podlet/archive/refs/tags/v0.3.2.tar.gz"
  end

  def install
    bin.install "podlet.exe"
  end

  test do
    expected_output = <<~EOS
      # FileName=hello
      [Container]
      Image=quay.io/podman/hello
    EOS

    assert_equal expected_output, shell_output("#{bin}/podlet podman run quay.io/podman/hello")
  end
end

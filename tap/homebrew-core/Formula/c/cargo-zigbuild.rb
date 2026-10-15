class CargoZigbuild < Formula
  desc "Compile Cargo project with zig as linker"
  homepage "https://github.com/rust-cross/cargo-zigbuild"
  url "https://github.com/rust-cross/cargo-zigbuild/releases/download/v0.23.4/cargo-zigbuild-x86_64-pc-windows-msvc.zip"
  sha256 "cd1226091f9f99ac7b46fb413968fb0b46edbe3ea961817d31b968352de4d4a6"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/rust-cross/cargo-zigbuild/archive/refs/tags/v0.23.4.tar.gz"
  end

  def install
    bin.install "cargo-zigbuild.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cargo-zigbuild --version 2>&1")
  end
end

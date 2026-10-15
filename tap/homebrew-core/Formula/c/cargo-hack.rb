class CargoHack < Formula
  desc "Cargo subcommand to provide options for testing and continuous integration"
  homepage "https://github.com/taiki-e/cargo-hack"
  url "https://github.com/taiki-e/cargo-hack/releases/download/v0.6.45/cargo-hack-x86_64-pc-windows-msvc.tar.gz"
  sha256 "9f796e7720686866f30bb51522efb2a2f875cb76cac41da61e3446b41f5fec9a"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/taiki-e/cargo-hack/archive/refs/tags/v0.6.45.tar.gz"
  end

  def install
    bin.install "cargo-hack.exe"
  end

  test do
    assert_path_exists bin/"cargo-hack.exe"
  end
end

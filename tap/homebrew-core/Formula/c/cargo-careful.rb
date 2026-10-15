class CargoCareful < Formula
  desc "Execute Rust code carefully, with extra checking along the way"
  homepage "https://github.com/RalfJung/cargo-careful"
  url "https://github.com/RalfJung/cargo-careful/releases/download/v0.4.10/cargo-careful-windows.exe"
  sha256 "89e2e95a6347595d73326b9ed014a58fb397f5a3cf9887ee8b9d92c181e20828"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/RalfJung/cargo-careful/archive/refs/tags/v0.4.10.tar.gz"
  end

  def install
    bin.install "cargo-careful-windows.exe" => "cargo-careful.exe"
  end

  test do
    assert_path_exists bin/"cargo-careful.exe"
  end
end

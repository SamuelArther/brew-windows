class Lemmeknow < Formula
  desc "Fastest way to identify anything!"
  homepage "https://github.com/swanandx/lemmeknow"
  url "https://github.com/swanandx/lemmeknow/releases/download/v0.8.0/lemmeknow-windows.exe"
  sha256 "6983af87d102bfe6fb00ed67ee64e7968908cb3242f8dee9e2eb981c297e6c66"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/swanandx/lemmeknow/archive/refs/tags/v0.8.0.tar.gz"
  end

  def install
    bin.install "lemmeknow-windows.exe" => "lemmeknow.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lemmeknow --version 2>&1")
  end
end

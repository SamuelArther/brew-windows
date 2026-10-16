class Dotter < Formula
  desc "Dotfile manager and templater written in rust"
  homepage "https://github.com/SuperCuber/dotter"
  url "https://github.com/SuperCuber/dotter/releases/download/v0.13.5/dotter-windows-x64-msvc.exe"
  sha256 "db2008898f2b81972a86f77f340f2ef44c4b3dffd01c61bba52d0bd804510c37"
  license "Unlicense"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/SuperCuber/dotter/archive/refs/tags/v0.13.5.tar.gz"
  end

  def install
    bin.install "dotter-windows-x64-msvc.exe" => "dotter.exe"
  end

  test do
    (testpath/"xxx.conf").write("12345678")
    (testpath/".dotter/local.toml").write <<~TOML
      packages = ["xxx"]
    TOML
    (testpath/".dotter/global.toml").write <<~TOML
      [xxx.files]
      "xxx.conf" = "yyy.conf"
    TOML

    system bin/"dotter", "deploy"
    assert_match "12345678", File.read("yyy.conf")
  end
end

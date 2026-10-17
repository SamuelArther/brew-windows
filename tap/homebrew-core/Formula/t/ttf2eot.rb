class Ttf2eot < Formula
  desc "Convert TTF files to EOT"
  homepage "https://github.com/wget/ttf2eot"
  url "https://github.com/wget/ttf2eot/archive/refs/tags/v0.0.3.tar.gz"
  sha256 "f363c4f2841b6d0b0545b30462e3c202c687d002da3d5dec7e2b827a032a3a65"
  license any_of: ["LGPL-2.0-or-later", "BSD-2-Clause"]

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "ae8098e1474ea98e848398bcf27b180b13bb65de33dc505169e82a1cafa86ae3"
  end

  def install
    system "make"
    bin.install "ttf2eot"
  end

  test do
    if OS.mac?
      font_name = "Arial Unicode"
      font_dir = "/Library/Fonts"
    elsif OS.windows?
      font_name = "arial"
      font_dir = "C:/Windows/Fonts"
    else
      font_name = "DejaVuSans"
      font_dir = "/usr/share/fonts/truetype/dejavu"
    end
    cp "#{font_dir}/#{font_name}.ttf", testpath
    system("#{bin}/ttf2eot < '#{font_name}.ttf' > '#{font_name}.eot'")
    assert_path_exists testpath/"#{font_name}.eot"
  end
end

class Svg2pdf < Formula
  desc "Renders SVG images to a PDF file (using Cairo)"
  homepage "https://cairographics.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-svg2pdf-0.13.0-1-any.pkg.tar.zst"
  sha256 "ad73752b82d507db8d410c900c17a620fd54ddda607d24ad2100cadc2883104a"
  license "HPND-sell-variant"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://cairographics.org/snapshots/"
    regex(/href=.*?svg2pdf[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/svg2pdf.exe", "svg2pdf"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/svg2pdf --version 2>&1")
  end
end

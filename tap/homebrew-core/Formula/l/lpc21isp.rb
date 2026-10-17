class Lpc21isp < Formula
  desc "In-circuit programming (ISP) tool for several NXP microcontrollers"
  homepage "https://lpc21isp.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/lpc21isp/lpc21isp/1.97/lpc21isp_197.tar.gz"
  version "1.97"
  sha256 "9f7d80382e4b70bfa4200233466f29f73a36fea7dc604e32f05b9aa69ef591dc"
  license "LGPL-3.0-or-later"

  no_autobump! because: :incompatible_version_format

  deny_network_access!

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/lpc21isp-1.97"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "82f6badd95e469a54c308338d8b6308bedaaa3e6f2ae29adc262f14adbe8683a"
  end

  def install
    system "make"
    bin.install ["lpc21isp"]
  end

  test do
    (testpath/"test.hex").write <<~EOS
      :0400000001020304F2
      :00000001FF
    EOS

    # Converts the Intel hex file (dumped to debugout.bin at debug level 4),
    # then fails to open the non-existent serial port
    output = shell_output("#{bin}/lpc21isp -debug4 -hex test.hex #{testpath}/nonexistent 115200 12000 2>&1", 2)
    assert_match "Image size : 4", output
    assert_match "Can't open COM-Port #{testpath}/nonexistent !", output
    assert_equal "\x01\x02\x03\x04".b, (testpath/"debugout.bin").binread
  end
end

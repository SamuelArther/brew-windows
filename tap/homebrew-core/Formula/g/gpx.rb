class Gpx < Formula
  desc "Gcode to x3g converter for 3D printers running Sailfish"
  homepage "https://github.com/markwal/GPX/blob/HEAD/README.md"
  url "https://github.com/markwal/GPX/releases/download/2.6.8/gpx-2.6.8-win64.zip"
  sha256 "4c99779a02d64efb95618bb6829d41462b9d8cbe24109dfad975140088dc73ab"
  license "GPL-2.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/markwal/GPX/archive/refs/tags/2.6.8.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/gpx.exe", "gpx"
    windows_exe "libexec/machines.exe", "machines"
    windows_exe "libexec/s3gdump.exe", "s3gdump"
  end

  test do
    (testpath/"test.gcode").write("G28 X Y Z")
    system bin/"gpx", "test.gcode"
  end
end

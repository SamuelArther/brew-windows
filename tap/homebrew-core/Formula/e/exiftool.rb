class Exiftool < Formula
  desc "Perl lib for reading and writing EXIF metadata"
  homepage "https://exiftool.org"
  url "https://downloads.sourceforge.net/project/exiftool/exiftool-13.59_64.zip"
  version "13.59"
  sha256 "44b512b25af500724ba579d0a53c8fc5851628b692dd5e5d94ae4a15c2cba9ec"
  license any_of: ["Artistic-1.0-Perl", "GPL-1.0-or-later"]

  # Official Windows Executable from exiftool.org (older releases are kept on its SourceForge mirror).

  def install
    # The "(-k)" in the upstream file name makes ExifTool wait for a key press; drop it.
    install_tree
    (libexec/"exiftool(-k).exe").rename libexec/"exiftool.exe"
    windows_exe "libexec/exiftool.exe", "exiftool"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/exiftool -ver")
  end
end

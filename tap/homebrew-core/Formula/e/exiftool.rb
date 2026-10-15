class Exiftool < Formula
  desc "Perl lib for reading and writing EXIF metadata"
  homepage "https://exiftool.org"
  version "13.55"
  url "https://downloads.sourceforge.net/project/exiftool/exiftool-13.55_64.zip"
  sha256 "9fadaf5221dcb07a5d018e21c8529e257917d2fad1fdfc8e64855c4fb73293df"
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

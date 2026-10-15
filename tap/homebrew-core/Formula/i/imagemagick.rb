# Official portable Q16-HDRI x64 build (Homebrew's imagemagick is Q16-HDRI). The classic tool
# names run through magick.exe, like Homebrew's symlinks.
class Imagemagick < Formula
  desc "Tools and libraries to manipulate images in select formats"
  homepage "https://imagemagick.org"
  url "https://github.com/ImageMagick/ImageMagick/releases/download/7.1.2-34/ImageMagick-7.1.2-34-portable-Q16-HDRI-x64.7z"
  version "7.1.2-34"
  sha256 "8dcfd3644b7fc20a92aeedeb4798024e5947f25f4d7c0c313c883fbca3d723e6"
  license "ImageMagick"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/magick.exe", "magick"
    windows_exe "libexec/magick.exe", "compare", "compare"
    windows_exe "libexec/magick.exe", "composite", "composite"
    windows_exe "libexec/magick.exe", "conjure", "conjure"
    windows_exe "libexec/magick.exe", "identify", "identify"
    windows_exe "libexec/magick.exe", "mogrify", "mogrify"
    windows_exe "libexec/magick.exe", "montage", "montage"
    windows_exe "libexec/magick.exe", "stream", "stream"
  end

  test do
    system bin/"magick", "-size", "16x16", "xc:red", testpath/"red.png"
    assert_match "PNG 16x16", shell_output("#{bin}/identify #{testpath}/red.png")
  end
end

class Firefox < Formula
  desc "Mozilla web browser"
  homepage "https://www.mozilla.org/firefox/"
  url "https://archive.mozilla.org/pub/firefox/releases/158.0/win64/multi/Firefox%20Setup%20158.0.msix"
  sha256 "21d0a9a0284ef97e2a6b117c1015844cd453475811a98d692651c357596968c9"
  license "MPL-2.0"

  # Mozilla MSIX payload extracted as a portable application. No Store registration, shortcuts, default-browser changes or GUI launch.

  def install
    install_tree
    windows_exe "libexec/VFS/ProgramFiles/MozillaFirefox%20Package%20Root/firefox.exe", "firefox"
  end
end

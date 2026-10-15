class Firefox < Formula
  desc "Mozilla web browser"
  homepage "https://www.mozilla.org/firefox/"
  url "https://archive.mozilla.org/pub/firefox/releases/158.0/win64/multi/Firefox%20Setup%20158.0.msix"
  sha256 "21d0a9a0284ef97e2a6b117c1015844cd453475811a98d692651c357596968c9"
  license "MPL-2.0"

  livecheck do
    url "https://product-details.mozilla.org/1.0/firefox_versions.json"
    strategy :json do |json|
      json["LATEST_FIREFOX_VERSION"]
    end
  end

  # Mozilla MSIX payload extracted as a portable application.
  # No Store registration, shortcuts, default-browser changes or GUI launch.

  def install
    install_tree
    windows_exe "libexec/VFS/ProgramFiles/MozillaFirefox%20Package%20Root/firefox.exe", "firefox"
  end

  test do
    # Firefox is a GUI app, so check the extracted payload instead of launching it.
    app = libexec/"VFS/ProgramFiles/MozillaFirefox%20Package%20Root"
    assert_path_exists app/"firefox.exe"
    assert_path_exists bin/"firefox.exe"
    assert_match "Version=#{version}", (app/"application.ini").read
  end
end

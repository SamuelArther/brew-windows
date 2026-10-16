class Nom < Formula
  desc "RSS reader for the terminal"
  homepage "https://github.com/guyfedwards/nom"
  url "https://github.com/guyfedwards/nom/releases/download/v3.3.3/nom_3.3.3_windows_amd64.tar.gz"
  sha256 "30f92bcca765fc291dd8a4fb902361272a47ca92447b896c64c05caba88d2f76"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/guyfedwards/nom/archive/refs/tags/v3.3.3.tar.gz"
  end

  def install
    bin.install "nom.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/nom version")

    assert_match "configpath", shell_output("#{bin}/nom config")
  end
end

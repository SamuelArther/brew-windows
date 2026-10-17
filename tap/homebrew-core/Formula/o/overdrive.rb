class Overdrive < Formula
  desc "Bash script to download mp3s from the OverDrive audiobook service"
  homepage "https://github.com/chbrown/overdrive"
  url "https://github.com/chbrown/overdrive/archive/refs/tags/2.4.1.tar.gz"
  sha256 "accc3ec4dab889f6bc003970be102ca7c85290b6516f71c8394e61946fb28860"
  license "MIT"
  revision 1
  head "https://github.com/chbrown/overdrive.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/overdrive-2.4.1_1"
    sha256 cellar: :any, x86_64_windows: "de3ca5fc8167bcc31a19f9a48a760e18ba25819f36267e324f7c42bebd01e3ae"
  end

  uses_from_macos "libxml2" # for xmllint
  uses_from_macos "openssl" # for openssl (non keg-only)

  on_linux do
    depends_on "util-linux" # for uuidgen
  end

  deny_network_access!

  def install
    bin.install "overdrive.sh" => "overdrive"
  end

  test do
    # A full run would require an authentic file, which can only be used once
    assert_match "Specified media file does not exist",
      shell_output("#{bin}/overdrive download fake_file.odm 2>&1", 2)
  end
end

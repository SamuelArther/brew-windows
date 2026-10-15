class SwitchLanPlay < Formula
  desc "Make you and your friends play games like in a LAN"
  homepage "https://github.com/spacemeowx2/switch-lan-play"
  url "https://github.com/spacemeowx2/switch-lan-play/releases/download/v0.2.3/lan-play-win64.exe"
  sha256 "c37b2609da4535ced94435fbc3815af96e7dda2f473008d1634e1d706fbf11af"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/spacemeowx2/switch-lan-play.git"
  end

  def install
    bin.install "lan-play-win64.exe" => "lan-play.exe"
  end

  test do
    assert_path_exists bin/"lan-play.exe"
  end
end

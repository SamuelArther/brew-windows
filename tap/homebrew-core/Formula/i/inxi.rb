class Inxi < Formula
  desc "Full featured CLI system information tool"
  homepage "https://smxi.org/docs/inxi.htm"
  url "https://codeberg.org/smxi/inxi/archive/3.3.41-1.tar.gz"
  sha256 "e08d92550a0f10890e722cc7db1e6d6cbde7e9fb47e61a8c6cec51f54a0b63d8"
  license "GPL-3.0-or-later"
  head "https://codeberg.org/smxi/inxi.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/inxi-3.3.41-1"
    sha256 cellar: :any, x86_64_windows: "8e662130ae749e7a7a96b28028641ee62ecc8ff66578f832528d8f70f11abdf2"
  end

  uses_from_macos "perl"

  def install
    bin.install "inxi"
    man1.install "inxi.1"

    # Build an `:all` bottle
    inreplace "inxi.changelog", "/usr/local/etc/inxi", "#{HOMEBREW_PREFIX}/etc/inxi"

    ["LICENSE.txt", "README.txt", "inxi.changelog"].each do |file|
      prefix.install file
    end
  end

  test do
    inxi_output = shell_output(bin/"inxi")
    uname_r = shell_output("uname -r").strip
    assert_match uname_r.to_str, inxi_output.to_s
  end
end

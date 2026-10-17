class Smenu < Formula
  desc "Powerful and versatile CLI selection tool for interactive or scripting use"
  homepage "https://github.com/p-gen/smenu"
  url "https://github.com/p-gen/smenu/releases/download/v1.5.0/smenu-1.5.0.tar.bz2"
  sha256 "2de2217d322a5e28cb20f9128e60df6c00b4e8e8879381ac8ed8bdcdccc4c5ca"
  license "MPL-2.0"

  # Exclude release candidate tags
  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2f181b735ae70bf3d73976072cf2b9635f82354fec11214fc9621d100fc8ccec"
  end

  uses_from_macos "ncurses"

  def install
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    require "pty"

    PTY.spawn(bin/"smenu", "--version") do |r, _w, _pid|
      r.winsize = [80, 60]

      begin
        assert_match version.to_s, r.read
      rescue Errno::EIO
        # GNU/Linux raises EIO when read is done on closed pty
      end
    end
  end
end

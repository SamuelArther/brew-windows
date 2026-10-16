class Libfuse < Formula
  desc "FUSE API for Windows (WinFsp's FUSE 2 and FUSE 3 compatibility layer)"
  homepage "https://winfsp.dev/"
  url "https://github.com/winfsp/winfsp/releases/download/v2.1/winfsp-2.1.25156.msi"
  sha256 "073a70e00f77423e34bed98b86e600def93393ba5822204fac57a29324db9f7a"
  license "GPL-3.0-only"

  # On Windows the FUSE interface is provided by WinFsp: its MSI (read as an archive, no installer
  # runs) carries the fuse/fuse3 headers, import libraries, pkg-config files and DLLs that FUSE file
  # systems build and link against.

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    # The MSI carries one .pc per architecture under the same name; point them at the x64 build
    # and at this keg's include/ and bin/.
    inreplace ["lib/fuse.pc", "lib/fuse3.pc"] do |s|
      s.gsub!(/^arch=.*$/, "arch=x64")
      s.gsub!("${prefix}/inc/", "${prefix}/include/")
    end
    install_tree "inc", "include"
    install_tree "lib", "lib"
    install_tree "SxS/DYNAMIC/bin", "libexec/bin"
    bin.install "SxS/DYNAMIC/bin/winfsp-x64.dll", "SxS/DYNAMIC/bin/winfsp-a64.dll", "SxS/DYNAMIC/bin/winfsp-x86.dll"
    install_tree "opt", "libexec/opt"
    install_tree "samples", "share/winfsp/samples"
  end

  def caveats
    <<~EOS
      FUSE file systems also need WinFsp's kernel driver, which a formula cannot install.
      Install it once with:
        winget install WinFsp.WinFsp
    EOS
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the FUSE 3 API headers and libraries instead.
    assert_match "FUSE_MAJOR_VERSION", (include/"fuse3/fuse_common.h").read
    assert_path_exists include/"fuse/fuse.h"
    assert_path_exists lib/"winfsp-x64.lib"
    assert_match "arch=x64", (lib/"fuse3.pc").read
    assert_path_exists bin/"winfsp-x64.dll"
  end
end

class GnustepMake < Formula
  desc "Basic GNUstep Makefiles"
  homepage "https://www.gnustep.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gnustep-make-2.9.3-2-any.pkg.tar.zst"
  sha256 "b87f5075b1c2f93a74ee9bda06d66754403080eab113cd148ee104b6357cb182"
  license "GPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/gnustep/tools-make/releases/download/make-2_9_3/gnustep-make-2.9.3.tar.gz"
    regex(/^make[._-]v?(\d+(?:[._]\d+)+)$/i)
    strategy :github_latest do |json, regex|
      json["tag_name"]&.scan(regex)&.map { |match| match[0].tr("_", ".") }
    end
  end

  def install
    install_tree
    install_library
  end

  test do
    assert_path_exists bin/"debugapp"
  end
end

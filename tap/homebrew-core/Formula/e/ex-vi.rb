class ExVi < Formula
  desc "UTF8-friendly version of traditional vi"
  homepage "https://ex-vi.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/ex-vi/ex-vi/050325/ex-050325.tar.bz2"
  sha256 "da4be7cf67e94572463b19e56850aa36dc4e39eb0d933d3688fe8574bb632409"
  license all_of: ["BSD-4-Clause", "BSD-4-Clause-UC"]

  livecheck do
    url :stable
    regex(%r{url=.*?/ex[._-]v?(\d+(?:\.\d+)*)\.t}i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/ex-vi-050325"
    sha256 cellar: :any, x86_64_windows: "156e7af4a9b0f9c11c904872b6a385334131b15e1df6169f13a83f50b4b91cf2"
  end

  uses_from_macos "ncurses"

  conflicts_with "macvim", because: "both install `vi` and `view` binaries"
  conflicts_with "vim", "vim-classic", because: "both install `ex` and `view` binaries"

  deny_network_access!

  def install
    # Fix compile with newer Clang
    ENV.append_to_cflags "-Wno-implicit-function-declaration" if DevelopmentTools.clang_build_version >= 1403
    ENV.deparallelize
    system "make", "install", "INSTALL=/usr/bin/install",
                              "PREFIX=#{prefix}",
                              "PRESERVEDIR=/var/tmp/vi.recover",
                              "TERMLIB=ncurses"
  end

  test do
    ENV["EXINIT"] = "set directory=#{testpath}"
    (testpath/"test.txt").write "hello\n"
    assert_match "\"test.txt\" 1 line, 6 characters", pipe_output("#{bin}/ex test.txt", "q\n", 0)
  end
end

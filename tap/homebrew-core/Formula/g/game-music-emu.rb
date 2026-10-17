class GameMusicEmu < Formula
  desc "Videogame music file emulator collection"
  homepage "https://github.com/libgme/game-music-emu"
  url "https://github.com/libgme/game-music-emu/archive/refs/tags/0.6.5.tar.gz"
  sha256 "8531678502451c2cf04248cda45c8b4645e19fcfb63e6a7ec2549641c47bb392"
  license one_of: ["LGPL-2.1-or-later", "GPL-2.0-or-later"]
  compatibility_version 1
  head "https://github.com/libgme/game-music-emu.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/game-music-emu-0.6.5"
    sha256 cellar: :any, x86_64_windows: "096fa1474272d817ff9d40780abe4802c5dc3fb941695e113c1e43a50a72a094"
  end

  depends_on "cmake" => :build

  on_linux do
    depends_on "zlib-ng-compat"
  end

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", "-DENABLE_UBSAN=OFF", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <gme/gme.h>
      int main(void)
      {
        Music_Emu* emu;
        gme_err_t error;

        error = gme_open_data((void*)0, 0, &emu, 44100);

        if (error == gme_wrong_file_type) {
          return 0;
        } else {
          return -1;
        }
      }
    C

    if OS.mac?
      ubsan_libdir = Dir["#{MacOS::CLT::PKG_PATH}/usr/lib/clang/*/lib/darwin"].first
      rpath = "-Wl,-rpath,#{ubsan_libdir}"
    end

    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", *(rpath if OS.mac?),
                   "-lgme", "-o", "test", *ENV.cflags.to_s.split
    system "./test"
  end
end

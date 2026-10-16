class WpebackendFdo < Formula
  desc "EGL and OpenGL ES platform for web view backends (ANGLE on Direct3D)"
  homepage "https://chromium.googlesource.com/angle/angle"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-angleproject-2.1.r25748.890b5d8f-6-any.pkg.tar.zst"
  version "2.1.r25748"
  sha256 "3c184061a3a3ef6478055e11eb786e20c40e62c1c239a1d65bf1399b7e2e7113"
  license "BSD-3-Clause"

  # WPEBackend-fdo gives WPE WebKit an EGL platform (Wayland EGL buffers exported to the host).
  # Windows has no native EGL: web engines there (Chromium, WebView2, Firefox) render GL ES through
  # ANGLE, Google's EGL/OpenGL ES implementation on Direct3D 11. This keg is ANGLE's libEGL and
  # libGLESv2 with the Khronos EGL/GLES headers, from MSYS2's UCRT64 packages, runtimes pinned below.

  livecheck do
    url "https://repo.msys2.org/mingw/ucrt64/"
    regex(/href=.*?mingw-w64-ucrt-x86_64-angleproject[._-]v?(\d+(?:\.\d+)+(?:\.r\d+)?)/i)
  end

  depends_on "libwpe"

  resource "mingw-w64-ucrt-x86_64-egl-headers" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-egl-headers-1.5.r284.3ae2b7c-1-any.pkg.tar.zst"
    sha256 "29743159048cf216f75d0dc3eb3e0a04a07515db17d839803e54b060f5aeb0b3"
  end

  resource "mingw-w64-ucrt-x86_64-gles-headers" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gles-headers-3.2.r1065.7fc154c-1-any.pkg.tar.zst"
    sha256 "066007a50bd08f062b5494f2c4811a219e1a65f4ecdce2beb453f6d9d8b92098"
  end

  resource "mingw-w64-ucrt-x86_64-jsoncpp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-jsoncpp-1.9.8-1-any.pkg.tar.zst"
    sha256 "c8bf6af2e6d7972f748f738ba8e510777437dcfd50846c378bc6e0990ce7ce2d"
  end

  resource "mingw-w64-ucrt-x86_64-libjpeg-turbo" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libjpeg-turbo-3.2.0-1-any.pkg.tar.zst"
    sha256 "7c001d00df557cfee969ad43d37b9c31ae22869e8aac4091a89dc3ae6d95e11d"
  end

  resource "mingw-w64-ucrt-x86_64-libpng" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libpng-1.6.59-1-any.pkg.tar.zst"
    sha256 "7f8448e06eceba8f8ce130390addea49f767bdf8f6b7e7eede8ca6d595e3d3a8"
  end

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  def install
    install_tree
    %w[egl-headers gles-headers].each do |name|
      install_resource "mingw-w64-ucrt-x86_64-#{name}", "libexec"
    end
    # Only the DLLs ANGLE loads: these libraries' headers and import libraries belong to the
    # jsoncpp, jpeg-turbo, libpng and zlib formulae, and would clash with them when linked.
    %w[jsoncpp libjpeg-turbo libpng zlib].each do |name|
      install_resource "mingw-w64-ucrt-x86_64-#{name}", "libexec/deps"
    end
    (libexec/"ucrt64/bin").install Dir["#{libexec}/deps/ucrt64/bin/*.dll"]
    rm_r libexec/"deps"
    install_library
  end

  def caveats
    <<~EOS
      This is EGL/OpenGL ES through ANGLE (Direct3D 11), not the wpe_fdo_* API: WPE WebKit has
      no Windows port. Link with -lEGL -lGLESv2 and include <EGL/egl.h> and <GLES2/gl2.h>.
    EOS
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the EGL/GLES API and the ANGLE libraries instead.
    assert_match "eglGetDisplay", (include/"EGL/egl.h").read
    assert_match "glDrawArrays", (include/"GLES2/gl2.h").read
    refute_empty Dir["#{prefix}/**/*EGL*.dll"]
    refute_empty Dir["#{prefix}/**/*GLESv2*.dll"]
  end
end

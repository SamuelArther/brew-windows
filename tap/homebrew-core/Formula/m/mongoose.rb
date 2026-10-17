class Mongoose < Formula
  desc "Web server build on top of Libmongoose embedded library"
  homepage "https://mongoose.ws/"
  url "https://github.com/cesanta/mongoose/archive/refs/tags/7.23.tar.gz"
  sha256 "93208f164038b05d156935b8b725063e1afb3984a362dbcf3a9ea37b6f1f2255"
  license "GPL-2.0-only"

  allow_network_access! :test

  bottle do
    sha256 cellar: :any, x86_64_windows: "ee41d8a0a52e6ce18126a4c5b6f2e2ce212c7bda1b436d073d3970772c442361"
  end

  def install
    # No Makefile but is an expectation upstream of binary creation
    # https://github.com/cesanta/mongoose/issues/326
    system "make", "-C", "tutorials/http/http-server", "example"
    bin.install "tutorials/http/http-server/example" => "mongoose"

    system ENV.cc, "-dynamiclib", "mongoose.c", "-o", "libmongoose.dylib" if OS.mac?
    if OS.linux?
      system ENV.cc, "-fPIC", "-c", "mongoose.c"
      system ENV.cc, "-shared", "-Wl,-soname,libmongoose.so", "-o", "libmongoose.so", "mongoose.o", "-lc", "-lpthread"
    end
    if OS.windows?
      system ENV.cc, "-shared", "mongoose.c", "-o", "libmongoose.dll",
             "-Wl,--out-implib,libmongoose.dll.a", "-lws2_32", "-lbcrypt"
      lib.install "libmongoose.dll.a"
    end
    lib.install shared_library("libmongoose")
    include.install "mongoose.h"
    pkgshare.install "tutorials"
    doc.install Dir["docs/*"]

    # Remove tutorials which have binaries built for a non-native architecture
    rm_r pkgshare/"tutorials/stm32/nucleo-n657x0-q/" if OS.linux?
  end

  test do
    (testpath/"hello.html").write <<~HTML
      <!DOCTYPE html>
      <html>
        <head>
          <title>Homebrew</title>
        </head>
        <body>
          <p>Hi!</p>
        </body>
      </html>
    HTML

    begin
      pid = fork { exec bin/"mongoose" }
      sleep 2
      assert_match "Hi!", shell_output("curl http://localhost:8000/hello.html")
    ensure
      Process.kill("SIGINT", pid)
      Process.wait(pid)
    end
  end
end

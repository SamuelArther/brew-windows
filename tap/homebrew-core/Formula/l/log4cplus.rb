class Log4cplus < Formula
  desc "Logging Framework for C++"
  homepage "https://sourceforge.net/p/log4cplus/wiki/Home/"
  url "https://downloads.sourceforge.net/project/log4cplus/log4cplus-stable/2.2.0/log4cplus-2.2.0.1.tar.xz"
  sha256 "6fc6b1b392921b048dcb0a71b5a1b46e9956bff710d288bdc9c4fc689f1a5f0b"
  license all_of: ["Apache-2.0", "BSD-2-Clause"]
  compatibility_version 1

  livecheck do
    url :stable
    regex(/url=.*?log4cplus-stable.*?log4cplus[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/log4cplus-2.2.0.1"
    sha256 cellar: :any, x86_64_windows: "bb030187f726bfab09d4271cc340b10777ae830142b0ee52d4a56bc5cdb313b2"
  end

  depends_on "pkgconf" => [:build, :test]

  def install
    ENV.cxx11
    system "./configure", *std_configure_args.reject { |s| s["--disable-debug"] }
    system "make", "install"
  end

  test do
    # https://github.com/log4cplus/log4cplus/blob/65e4c3/docs/examples.md
    (testpath/"test.cpp").write <<~CPP
      #include <log4cplus/logger.h>
      #include <log4cplus/loggingmacros.h>
      #include <log4cplus/configurator.h>
      #include <log4cplus/initializer.h>

      int main()
      {
        log4cplus::Initializer initializer;
        log4cplus::BasicConfigurator config;
        config.configure();

        log4cplus::Logger logger = log4cplus::Logger::getInstance(
          LOG4CPLUS_TEXT("main"));
        LOG4CPLUS_WARN(logger, LOG4CPLUS_TEXT("Hello, World!"));
        return 0;
      }
    CPP

    pkgconf_flags = shell_output("pkgconf --cflags --libs log4cplus").chomp.split
    system ENV.cxx, "-std=c++11", "-o", "test", "test.cpp", *pkgconf_flags
    assert_match "Hello, World!", shell_output("./test")
  end
end

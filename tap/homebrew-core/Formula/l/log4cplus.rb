class Log4cplus < Formula
  desc "Logging Framework for C++"
  homepage "https://github.com/log4cplus/log4cplus/wiki"
  url "https://github.com/log4cplus/log4cplus/releases/download/REL_2_2_1/log4cplus-2.2.1.tar.xz"
  sha256 "4b2801aedcdb32476a6f139a93ca01c9dac5158682516ca5a02df68c600292c6"
  license all_of: ["Apache-2.0", "BSD-2-Clause"]
  compatibility_version 1

  livecheck do
    url :stable
    regex(/^REL[._-]v?(\d+(?:[._]\d+)+)$/i)
    strategy :git do |tags, regex|
      tags.filter_map { |tag| tag[regex, 1]&.tr("_", ".") }
    end
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "b3620fff892651919811c8af3bdbc413d23d89a6f1110102b95319368993068b"
  end

  depends_on "pkgconf" => [:build, :test]

  deny_network_access!

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

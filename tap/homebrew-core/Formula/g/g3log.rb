class G3log < Formula
  desc "Asynchronous, 'crash safe', logger that is easy to use"
  homepage "https://github.com/KjellKod/g3log"
  url "https://github.com/KjellKod/g3log/archive/refs/tags/2.6.1.tar.gz"
  sha256 "65cb6e56e7757221fd4f3f6e97a47834cacf45288010f8fa2b192272eaa63637"
  license "Unlicense"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a8e3e1b67717fad608be0596d6bed96f1e902eed8ad93fe236924f4a99764c5b"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", "-DADD_G3LOG_UNIT_TEST=OFF", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <g3log/g3log.hpp>
      #include <g3log/logworker.hpp>
      int main()
      {
        auto worker = g3::LogWorker::createLogWorker();
        worker->addDefaultLogger("test", "#{testpath}");
        g3::initializeLogging(worker.get());
        LOG(DEBUG) << "Hello World";
        return 0;
      }
    CPP
    system ENV.cxx, "-std=c++17", "test.cpp", "-L#{lib}", "-lg3log", "-o", "test"
    system "./test"

    log = testpath.glob("test.g3log.*.log").first
    refute_nil log, "Expected log file"
    assert_match "\tDEBUG [test.cpp->main:8]\tHello World\n", log.read
  end
end

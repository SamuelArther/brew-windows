class Serialize < Formula
  desc "Single-header bitpacking serializer for C++ aimed at game networking"
  homepage "https://github.com/mas-bandwidth/serialize"
  url "https://github.com/mas-bandwidth/serialize/archive/refs/tags/v1.17.0.tar.gz"
  sha256 "8bde39558934f51cdc3d1cedbfbb4061cfda2b78103eb75a12e09428249436b9"
  license "BSD-3-Clause"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/serialize-1.17.0"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "16bc687aae5ca545da0ace10c756964eaeea512eec6c2502aa477b4cd6ff6bfb"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", "-DSERIALIZE_BUILD_TESTS=OFF", *std_cmake_args
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <serialize.h>
      int main()
      {
          uint8_t buffer[64];
          memset( buffer, 0, sizeof( buffer ) );
          serialize::WriteStream writer( buffer, 64 );
          uint32_t written = 12345;
          writer.SerializeBits( written, 14 );
          writer.Flush();
          serialize::ReadStream reader( buffer, writer.GetBytesProcessed() );
          uint32_t value = 0;
          reader.SerializeBits( value, 14 );
          return value == 12345 ? 0 : 1;
      }
    CPP
    system ENV.cxx, "test.cpp", "-I#{include}", "-o", "test"
    system "./test"
  end
end

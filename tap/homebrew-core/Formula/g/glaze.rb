class Glaze < Formula
  desc "Extremely fast, in-memory JSON and interface library for modern C++"
  homepage "https://stephenberry.github.io/glaze/"
  url "https://github.com/stephenberry/glaze/archive/refs/tags/v9.0.0.tar.gz"
  sha256 "dd7b033c5bf6e4308615bb7834c541291d5b20f64121d9a68222a39414635517"
  license "MIT"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/glaze-9.0.0"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2f9b950ce8ff5f55999dc5082b5a9d551617f719d456c1eff2fe204e36e3ce5b"
  end

  depends_on "cmake" => [:build, :test]

  deny_network_access!

  def install
    args = %w[
      -Dglaze_DEVELOPER_MODE=OFF
    ]
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"CMakeLists.txt").write <<~CMAKE
      cmake_minimum_required(VERSION 4.0)
      project(GlazeTest LANGUAGES CXX)

      set(CMAKE_CXX_STANDARD 20)
      set(CMAKE_CXX_STANDARD_REQUIRED ON)

      find_package(glaze REQUIRED)

      add_executable(glaze_test test.cpp)
      target_link_libraries(glaze_test PRIVATE glaze::glaze)
    CMAKE

    (testpath/"test.cpp").write <<~CPP
      #include <glaze/glaze.hpp>
      #include <map>
      #include <string_view>

      int main() {
        const std::string_view json = R"({"key": "value"})";
        std::map<std::string, std::string> data;
        auto result = glz::read_json(data, json);
        return (!result && data["key"] == "value") ? 0 : 1;
      }
    CPP

    system "cmake", "-S", ".", "-B", "build", "-Dglaze_DIR=#{share}/glaze"
    system "cmake", "--build", "build"
    system "./build/glaze_test"
  end
end

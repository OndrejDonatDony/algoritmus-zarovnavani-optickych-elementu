classdef ChunkDetector < handle
    properties
        G
        BW
        Centroids
    end

    methods
        function obj = ChunkDetector(img)
            if size(img,3) == 3
                obj.G = rgb2gray(img);
            else
                obj.G = img;
            end
            obj.BW = [];
            obj.Centroids = [];
        end

        function BW = BinaryImg(obj, img)
            if nargin < 2
                img = obj.G;
            end
        
            if size(img,3) == 3
                G = rgb2gray(img);
            else
                G = img;
            end
        
            if nnz(G > 1) < 1500
                BW = G > 1;
            else
                for TH = 1:255
                    BW = G > TH;
                    if nnz(BW) < 1500
                        break;
                    end
                end
            end
        
            BW = bwareaopen(BW, 3);
            obj.BW = BW;
        end
        
    end
end
